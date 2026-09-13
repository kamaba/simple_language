# =========================================================================
# SLMysql —— MySQL 客户端库（Python DB-API 2.0 / PyMySQL 风味）
#
# 用法（对照 Python）：
#
#   conn = SL.Mysql.connect( "127.0.0.1", "root", "123456", "test" )
#   cur  = conn.cursor()
#
#   cur.execute( "INSERT INTO user(name, age) VALUES (?, ?)", Tuple("alice", 18) )
#   conn.commit()
#
#   cur.execute( "SELECT * FROM user" )
#   for i = 0, i < cur.columnCount(), i++
#   {
#       Console.println( cur.columnName(i) )
#   }
#   var row = cur.fetchone()                 # 一行，读完返回 null
#   var rows = cur.fetchall()                # 全部行，List<MysqlRow>
#   for i = 0, i < rows.length, i++
#   {
#       Console.println( rows._getItem_(i).toString() )
#   }
#
#   cur.close()
#   conn.close()
#
# 错误处理沿用 Python DB-API 的"异常即错误"思路，但以返回值表达（与本仓库
# Std/DB/Sqlite.sl 保持一致）：connect 失败返回 null，其余失败返回 false。
# 具体原因通过 conn.errorCode() / conn.errorMessage() 取得。
#
# 底层链路：
#   SLMysql.sl ──@DllStaticImport(opcode 118)──> sl_mysql_lib.dll
#              ──静态链接──────────────────────> MySQL Connector/C
#                                                (mysqlclient.lib)
# sl_mysql_lib.dll 把 MYSQL / MYSQL_RES 收敛为 Int64 句柄，把数据收敛为
# bool / Int32 / Int64 / string，因此可以完全静态绑定直调。
#
# 注意：所有类型必须是 namespace 下的顶级类型（不能嵌套在类内）。
# =========================================================================

namespace SL
{
    # ===================================================================
    # Mysql —— 入口类 + 低层绑定
    #
    # lib* 系列是 sl_mysql_lib.dll 的直接映射（函数体 = DLL 不可用时的
    # 兜底返回值），通常不必直接使用；connect() / clientVersion() 是给
    # 业务用的门面。
    # ===================================================================
    public class Mysql
    {
        # ── 低层：连接生命周期 ────────────────────────────────────

        @DllStaticImport( "sl_mysql_lib", "slm_init" )
        public static Int64 libInit()
        {
            ret 0
        }

        @DllStaticImport( "sl_mysql_lib", "slm_close" )
        public static void libClose( Int64 conn )
        {
        }

        @DllStaticImport( "sl_mysql_lib", "slm_connect" )
        public static bool libConnect( Int64 conn, string host, string user, string password, string database, Int32 port )
        {
            ret false
        }

        @DllStaticImport( "sl_mysql_lib", "slm_select_db" )
        public static bool libSelectDb( Int64 conn, string database )
        {
            ret false
        }

        @DllStaticImport( "sl_mysql_lib", "slm_set_charset" )
        public static bool libSetCharset( Int64 conn, string charset )
        {
            ret false
        }

        @DllStaticImport( "sl_mysql_lib", "slm_ping" )
        public static bool libPing( Int64 conn )
        {
            ret false
        }

        # ── 低层：事务 ────────────────────────────────────────────

        @DllStaticImport( "sl_mysql_lib", "slm_autocommit" )
        public static bool libAutocommit( Int64 conn, bool on )
        {
            ret false
        }

        @DllStaticImport( "sl_mysql_lib", "slm_commit" )
        public static bool libCommit( Int64 conn )
        {
            ret false
        }

        @DllStaticImport( "sl_mysql_lib", "slm_rollback" )
        public static bool libRollback( Int64 conn )
        {
            ret false
        }

        # ── 低层：诊断 ────────────────────────────────────────────

        @DllStaticImport( "sl_mysql_lib", "slm_errno" )
        public static Int32 libErrno( Int64 conn )
        {
            ret 0
        }

        @DllStaticImport( "sl_mysql_lib", "slm_error" )
        public static string libError( Int64 conn )
        {
            ret "sl_mysql_lib not available"
        }

        @DllStaticImport( "sl_mysql_lib", "slm_client_version" )
        public static Int64 libClientVersion()
        {
            ret 0
        }

        @DllStaticImport( "sl_mysql_lib", "slm_client_info" )
        public static string libClientInfo()
        {
            ret "unknown"
        }

        # ── 低层：执行 ────────────────────────────────────────────

        @DllStaticImport( "sl_mysql_lib", "slm_query" )
        public static bool libQuery( Int64 conn, string sql )
        {
            ret false
        }

        @DllStaticImport( "sl_mysql_lib", "slm_store_result" )
        public static Int64 libStoreResult( Int64 conn )
        {
            ret 0
        }

        @DllStaticImport( "sl_mysql_lib", "slm_use_result" )
        public static Int64 libUseResult( Int64 conn )
        {
            ret 0
        }

        @DllStaticImport( "sl_mysql_lib", "slm_affected_rows" )
        public static Int64 libAffectedRows( Int64 conn )
        {
            ret 0
        }

        @DllStaticImport( "sl_mysql_lib", "slm_insert_id" )
        public static Int64 libInsertId( Int64 conn )
        {
            ret 0
        }

        @DllStaticImport( "sl_mysql_lib", "slm_field_count" )
        public static Int32 libFieldCount( Int64 conn )
        {
            ret 0
        }

        # ── 低层：结果集 ──────────────────────────────────────────

        @DllStaticImport( "sl_mysql_lib", "slm_free_result" )
        public static void libFreeResult( Int64 res )
        {
        }

        @DllStaticImport( "sl_mysql_lib", "slm_num_rows" )
        public static Int64 libNumRows( Int64 res )
        {
            ret 0
        }

        @DllStaticImport( "sl_mysql_lib", "slm_num_fields" )
        public static Int32 libNumFields( Int64 res )
        {
            ret 0
        }

        @DllStaticImport( "sl_mysql_lib", "slm_field_name" )
        public static string libFieldName( Int64 res, Int32 index )
        {
            ret ""
        }

        @DllStaticImport( "sl_mysql_lib", "slm_field_type" )
        public static Int32 libFieldType( Int64 res, Int32 index )
        {
            ret 0
        }

        # ── 低层：行游标 ──────────────────────────────────────────

        @DllStaticImport( "sl_mysql_lib", "slm_fetch_row" )
        public static bool libFetchRow( Int64 res )
        {
            ret false
        }

        @DllStaticImport( "sl_mysql_lib", "slm_row_value" )
        public static string libRowValue( Int64 res, Int32 index )
        {
            ret ""
        }

        @DllStaticImport( "sl_mysql_lib", "slm_row_length" )
        public static Int64 libRowLength( Int64 res, Int32 index )
        {
            ret 0
        }

        @DllStaticImport( "sl_mysql_lib", "slm_row_is_null" )
        public static bool libRowIsNull( Int64 res, Int32 index )
        {
            ret true
        }

        # ── 低层：转义 ────────────────────────────────────────────

        @DllStaticImport( "sl_mysql_lib", "slm_escape" )
        public static string libEscape( Int64 conn, string text )
        {
            ret ""
        }

        # ── 门面：连接 ────────────────────────────────────────────

        # connect(host, user, password, database) —— 默认 3306 / utf8mb4
        public static MysqlConnection connect( string host, string user, string password, string database )
        {
            ret Mysql.connect( host, user, password, database, 3306, "utf8mb4" )
        }

        # connect(host, user, password, database, port) —— 默认 utf8mb4
        public static MysqlConnection connect( string host, string user, string password, string database, int port )
        {
            ret Mysql.connect( host, user, password, database, port, "utf8mb4" )
        }

        # connect(host, user, password, database, port, charset) —— 完整形式
        # 成功返回连接对象，失败返回 null（原因拿不到：连接对象尚未建立，
        # 失败信息只在 C 层，见 cvm 运行日志）。
        public static MysqlConnection connect( string host, string user, string password, string database, int port, string charset )
        {
            Int64 conn = Mysql.libInit()
            if conn == 0
            {
                ret null
            }
            if charset != ""
            {
                Mysql.libSetCharset( conn, charset )
            }
            if Mysql.libConnect( conn, host, user, password, database, port ) == false
            {
                Mysql.libClose( conn )
                ret null
            }
            ret MysqlConnection( conn )
        }

        # ── 门面：客户端信息 ──────────────────────────────────────

        public static Int64 clientVersion()
        {
            ret Mysql.libClientVersion()
        }

        public static string clientInfo()
        {
            ret Mysql.libClientInfo()
        }

        # ── 门面：按连接字符集转义（不含引号） ────────────────────

        public static string escape( Int64 conn, string text )
        {
            ret Mysql.libEscape( conn, text )
        }

        # 把一个值转成可直接拼进 SQL 的字面量：
        # null -> NULL；其余一律按字符串转义并加单引号（MySQL 会按列类型
        # 自动转换，故数值也能正确落库）。
        public static string literal( Int64 conn, object value )
        {
            if value == null
            {
                ret "NULL"
            }
            ret "'" + Mysql.libEscape( conn, value.toString() ) + "'"
        }

        # 把 sql 中的 "?" 依次替换为 args 的字面量（Python DB-API 的
        # qmark 风格）。字符串字面量内部的 '?' 同样会被替换——这是本实现的
        # 已知取舍，需要写死 '?' 时请改用双写并按实际情况调整。
        public static string bindParams( Int64 conn, string sql, Tuple args )
        {
            if args == null || args.length == 0
            {
                ret sql
            }

            string result = ""
            int argIndex = 0
            int count = args.length
            int n = sql.length
            for i = 0, i < n, i++
            {
                string ch = sql.range( i, i + 1 )
                if ch == "?" && argIndex < count
                {
                    result = result + Mysql.literal( conn, args._getItem_( argIndex ) )
                    argIndex++
                }
                else
                {
                    result = result + ch
                }
            }
            ret result
        }
    }

    # ===================================================================
    # MysqlConnection —— 数据库连接（仿 Python Connection）
    # ===================================================================
    public class MysqlConnection
    {
        Int64 _handle = 0

        _init_( Int64 handle )
        {
            this._handle = handle
        }

        # 原始句柄（诊断用途）
        get Int64 handle()
        {
            ret this._handle
        }

        get bool isClosed()
        {
            ret this._handle == 0
        }

        # ── 游标 ──────────────────────────────────────────────────

        public MysqlCursor cursor()
        {
            ret MysqlCursor( this._handle )
        }

        # execute(sql) —— 便捷：建游标并执行
        public MysqlCursor execute( string sql )
        {
            MysqlCursor cur = this.cursor()
            cur.execute( sql )
            ret cur
        }

        # execute(sql, args) —— 便捷：带参数
        public MysqlCursor execute( string sql, Tuple args )
        {
            MysqlCursor cur = this.cursor()
            cur.execute( sql, args )
            ret cur
        }

        # ── 会话 ──────────────────────────────────────────────────

        public bool selectDatabase( string database )
        {
            ret Mysql.libSelectDb( this._handle, database )
        }

        public bool setCharset( string charset )
        {
            ret Mysql.libSetCharset( this._handle, charset )
        }

        public bool ping()
        {
            ret Mysql.libPing( this._handle )
        }

        # ── 事务 ──────────────────────────────────────────────────

        public bool autocommit( bool on )
        {
            ret Mysql.libAutocommit( this._handle, on )
        }

        public bool commit()
        {
            ret Mysql.libCommit( this._handle )
        }

        public bool rollback()
        {
            ret Mysql.libRollback( this._handle )
        }

        # ── 诊断 / 统计 ───────────────────────────────────────────

        public Int32 errorCode()
        {
            ret Mysql.libErrno( this._handle )
        }

        public string errorMessage()
        {
            ret Mysql.libError( this._handle )
        }

        public Int64 affectedRows()
        {
            ret Mysql.libAffectedRows( this._handle )
        }

        public Int64 insertId()
        {
            ret Mysql.libInsertId( this._handle )
        }

        # ── 关闭 ──────────────────────────────────────────────────

        public void close()
        {
            if this._handle != 0
            {
                Mysql.libClose( this._handle )
                this._handle = 0
            }
        }
    }

    # ===================================================================
    # MysqlCursor —— 游标（仿 Python Cursor）
    # ===================================================================
    public class MysqlCursor
    {
        Int64 _conn = 0
        Int64 _res = 0
        Int32 _fieldCount = 0
        bool _hasResult = false

        _init_( Int64 conn )
        {
            this._conn = conn
        }

        # ── 执行 ──────────────────────────────────────────────────

        public MysqlCursor execute( string sql )
        {
            ret this._run( sql, null )
        }

        public MysqlCursor execute( string sql, Tuple args )
        {
            ret this._run( sql, args )
        }

        MysqlCursor _run( string sql, Tuple args )
        {
            this._releaseResult()

            if args == null
            {
                if Mysql.libQuery( this._conn, sql ) == false
                {
                    ret this
                }
            }
            else
            {
                if Mysql.libQuery( this._conn, Mysql.bindParams( this._conn, sql, args ) ) == false
                {
                    ret this
                }
            }

            # 非查询语句没有结果集，此时 affectedRows / insertId 有意义
            if Mysql.libFieldCount( this._conn ) > 0
            {
                Int64 res = Mysql.libStoreResult( this._conn )
                if res != 0
                {
                    this._res = res
                    this._fieldCount = Mysql.libNumFields( res )
                    this._hasResult = true
                }
            }
            ret this
        }

        # ── 取行 ──────────────────────────────────────────────────

        # fetchone() —— 取一行，读完返回 null
        public MysqlRow fetchone()
        {
            if this._hasResult == false
            {
                ret null
            }
            if Mysql.libFetchRow( this._res ) == false
            {
                ret null
            }
            ret MysqlRow( this._res, this._fieldCount )
        }

        # fetchall() —— 一次取完，返回 List<MysqlRow>
        public List<MysqlRow> fetchall()
        {
            List<MysqlRow> rows = List<MysqlRow>()
            MysqlRow row = this.fetchone()
            while row != null
            {
                rows.add( row )
                row = this.fetchone()
            }
            ret rows
        }

        # fetchmany(count) —— 取至多 count 行
        public List<MysqlRow> fetchmany( int count )
        {
            List<MysqlRow> rows = List<MysqlRow>()
            if count <= 0
            {
                ret rows
            }
            int got = 0
            MysqlRow row = this.fetchone()
            while row != null && got < count
            {
                rows.add( row )
                got++
                if got >= count
                {
                    ret rows
                }
                row = this.fetchone()
            }
            ret rows
        }

        # ── 结果集元信息 ──────────────────────────────────────────

        public int columnCount()
        {
            ret this._fieldCount
        }

        public string columnName( int index )
        {
            if this._hasResult == false
            {
                ret ""
            }
            ret Mysql.libFieldName( this._res, index )
        }

        # 列类型（mysql_com.h enum_field_types）
        public int columnType( int index )
        {
            if this._hasResult == false
            {
                ret 0
            }
            ret Mysql.libFieldType( this._res, index )
        }

        public List<string> columnNames()
        {
            List<string> names = List<string>()
            for i = 0, i < this._fieldCount, i++
            {
                names.add( Mysql.libFieldName( this._res, i ) )
            }
            ret names
        }

        public Int64 rowCount()
        {
            if this._hasResult == false
            {
                ret 0
            }
            ret Mysql.libNumRows( this._res )
        }

        # ── 关闭 ──────────────────────────────────────────────────

        public void close()
        {
            this._releaseResult()
        }

        void _releaseResult()
        {
            if this._res != 0
            {
                Mysql.libFreeResult( this._res )
                this._res = 0
            }
            this._fieldCount = 0
            this._hasResult = false
        }
    }

    # ===================================================================
    # MysqlRow —— 一行数据（仿 Python 的 row tuple）
    #
    # 构造时即把当前行各列物化为字符串（SQL NULL 记为 null），因此 Row
    # 的生命周期与游标解耦——可以继续 fetch / close 游标而不会失效。
    # ===================================================================
    public class MysqlRow
    {
        Array<object> _values = null
        Int32 _count = 0

        _init_( Int64 res, Int32 columnCount )
        {
            this._count = columnCount
            this._values = Array<object>( columnCount )
            for i = 0, i < columnCount, i++
            {
                SystemArraySetValueThis( this._values, i, Mysql.libRowValue( res, i ) )
            }
        }

        get int length()
        {
            ret this._count
        }

        override public object _getItem_( int index )
        {
            if index < 0 || index >= this._count
            {
                ret null
            }
            ret SystemArrayGetValueThis( this._values, index )
        }

        # 是否为 SQL NULL
        public bool isNull( int index )
        {
            ret this._getItem_( index ) == null
        }

        # 取列值的字符串形式；SQL NULL 返回空串
        public string getString( int index )
        {
            var v = this._getItem_( index )
            if v == null
            {
                ret ""
            }
            ret v.toString()
        }

        override string toString()
        {
            string s = "("
            for i = 0, i < this._count, i++
            {
                var v = SystemArrayGetValueThis( this._values, i )
                if v == null
                {
                    s = s + "None"
                }
                else
                {
                    s = s + "'" + v.toString() + "'"
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
}
