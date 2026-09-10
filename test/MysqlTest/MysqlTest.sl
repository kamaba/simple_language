import Std;
import SLMysql;

# =========================================================================
# MysqlTest —— SLMysql 端到端测试（连接真实 MySQL 服务器）
#
# 覆盖：
#   1) 原生层可用性（clientVersion / clientInfo —— 非 fallback 说明
#      sl_mysql_lib.dll 已静态预载，@DllStaticImport 直调生效）
#   2) 连接 / 建库 / 切库 / 建表
#   3) CRUD（INSERT / SELECT / UPDATE / DELETE 与 affectedRows / insertId）
#   4) 结果集元信息（columnCount / columnName / rowCount / row 取值）
#   5) 清理（DROP DATABASE）
#
# SLMysql 的类都在 namespace SL 下，故统一用 SL.xxx 全名访问。
# 连接参数来自 ProjectTest.jsonc 的 "data" 段，按 global.<name> 读取。
# =========================================================================
class MysqlTest
{
    static int s_pass = 0
    static int s_fail = 0

    static void check( string name, bool ok )
    {
        if ok
        {
            s_pass++
            Console.println( "  [PASS] " + name )
        }
        else
        {
            s_fail++
            Console.println( "  [FAIL] " + name )
        }
    }

    # 连接指定数据库
    static SL.MysqlConnection connectTo( string db )
    {
        ret SL.Mysql.connect( global.mysqlHost, global.mysqlUser, global.mysqlPassword, db, global.mysqlPort, global.mysqlCharset )
    }

    static fun()
    {
        Console.println( "" )
        Console.println( "===== MysqlTest start =====" )

        # ── 1. 原生层可用性 ──────────────────────────────────────
        Int64 ver = SL.Mysql.clientVersion()
        Console.println( "clientVersion = " + ver.toString() )
        check( "T01 clientVersion > 0 (DLL loaded, not SL fallback)", ver > 0 )

        string info = SL.Mysql.clientInfo()
        Console.println( "clientInfo = " + info )
        check( "T02 clientInfo not empty", info != "" )

        # ── 2. 连接 ──────────────────────────────────────────────
        SL.MysqlConnection conn = connectTo( global.mysqlDatabase )
        bool connected = false
        if conn == null
        {
            connected = false
        }
        else
        {
            connected = true
        }
        check( "T03 connect ok", connected )
        if connected == false
        {
            Console.println( "connect failed, skip rest of MysqlTest" )
            ret
        }

        check( "T04 errorCode == 0 after connect", conn.errorCode() == 0 )
        check( "T05 ping ok", conn.ping() )
        check( "T06 isClosed == false", conn.isClosed() == false )

        # ── 3. 建库 / 切库 ───────────────────────────────────────
        string testdb = global.mysqlTestDb
        Console.println( "test database = " + testdb )

        SL.MysqlCursor cur = conn.cursor()

        cur.execute( "DROP DATABASE IF EXISTS " + testdb )
        cur.execute( "CREATE DATABASE " + testdb + " DEFAULT CHARACTER SET utf8mb4" )
        check( "T07 CREATE DATABASE " + testdb, conn.errorCode() == 0 )
        Console.println( "  create db err = [" + conn.errorMessage() + "]" )
        cur.close()

        bool switched = conn.selectDatabase( testdb )
        check( "T08 selectDatabase to test db", switched )

        # ── 4. 建表 ──────────────────────────────────────────────
        string ddl = "CREATE TABLE user ("
        ddl = ddl + " id INT NOT NULL AUTO_INCREMENT,"
        ddl = ddl + " name VARCHAR(64) NOT NULL,"
        ddl = ddl + " age INT DEFAULT 0,"
        ddl = ddl + " email VARCHAR(128) NULL,"
        ddl = ddl + " PRIMARY KEY (id)"
        ddl = ddl + " ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4"

        cur.execute( ddl )
        check( "T09 CREATE TABLE user", conn.errorCode() == 0 )
        Console.println( "  create table err = [" + conn.errorMessage() + "]" )

        SL.MysqlCursor tcur = conn.cursor()
        tcur.execute( "SHOW TABLES" )
        Int64 tblRows = tcur.rowCount()
        check( "T10 SHOW TABLES sees user", tcur.columnCount() == 1 && tblRows == 1 )
        Console.println( "  show tables rows = " + tblRows.toString() )
        tcur.close()

        # ── 5. INSERT（? 参数绑定）───────────────────────────────
        cur.execute( "INSERT INTO user (name, age, email) VALUES (?, ?, ?)", Tuple( "alice", 18, "alice@test.com" ) )
        check( "T11 INSERT affectedRows == 1", conn.affectedRows() == 1 )
        Int64 firstId = conn.insertId()
        check( "T12 insertId > 0", firstId > 0 )
        Console.println( "  first insertId = " + firstId.toString() )

        cur.execute( "INSERT INTO user (name, age, email) VALUES (?, ?, ?)", Tuple( "bob", 20, "bob@test.com" ) )
        cur.execute( "INSERT INTO user (name, age, email) VALUES (?, ?, ?)", Tuple( "carol", 22, "carol@test.com" ) )
        check( "T13 INSERT affectedRows == 1", conn.affectedRows() == 1 )

        # ── 6. SELECT / 结果集读取 ───────────────────────────────
        cur.execute( "SELECT id, name, age, email FROM user ORDER BY id" )
        check( "T14 SELECT columnCount == 4", cur.columnCount() == 4 )
        check( "T15 SELECT rowCount == 3", cur.rowCount() == 3 )
        check( "T16 columnName(0) == id", cur.columnName( 0 ) == "id" )
        check( "T17 columnName(1) == name", cur.columnName( 1 ) == "name" )

        List<string> names = cur.columnNames()
        check( "T18 columnNames().length == 4", names.length == 4 )

        var rows = cur.fetchall()
        check( "T19 fetchall rows == 3", rows.length == 3 )
        if rows.length >= 3
        {
            SL.MysqlRow r1 = rows._getItem_( 0 )
            check( "T20 row name == alice", r1.getString( 1 ) == "alice" )
            check( "T21 row age == 18", r1.getString( 2 ) == "18" )
            check( "T22 row length == 4", r1.length == 4 )
            check( "T23 row not null value", r1.isNull( 1 ) == false )
            Console.println( "  row0 = " + r1.toString() )
        }

        # ── 7. UPDATE ────────────────────────────────────────────
        cur.execute( "UPDATE user SET age = ? WHERE name = ?", Tuple( 30, "alice" ) )
        check( "T24 UPDATE affectedRows == 1", conn.affectedRows() == 1 )

        cur.execute( "SELECT age FROM user WHERE name = 'alice'" )
        SL.MysqlRow ur = cur.fetchone()
        bool ageOk = false
        if ur == null
        {
            ageOk = false
        }
        else
        {
            ageOk = ur.getString( 0 ) == "30"
        }
        check( "T25 UPDATE age == 30", ageOk )
        check( "T26 fetchone returns null after last row", cur.fetchone() == null )

        # ── 8. DELETE ────────────────────────────────────────────
        cur.execute( "DELETE FROM user WHERE name = ?", Tuple( "bob" ) )
        check( "T27 DELETE affectedRows == 1", conn.affectedRows() == 1 )

        cur.execute( "SELECT COUNT(*) AS c FROM user" )
        SL.MysqlRow cnt = cur.fetchone()
        bool cntOk = false
        if cnt == null
        {
            cntOk = false
        }
        else
        {
            cntOk = cnt.getString( 0 ) == "2"
        }
        check( "T28 rows left == 2 after delete", cntOk )

        # ── 9. 清理 ──────────────────────────────────────────────
        cur.execute( "DROP TABLE IF EXISTS user" )
        check( "T29 DROP TABLE user", conn.errorCode() == 0 )

        cur.close()
        conn.close()
        check( "T30 isClosed after close", conn.isClosed() )

        SL.MysqlConnection c2 = connectTo( global.mysqlDatabase )
        if c2 != null
        {
            SL.MysqlCursor dcur = c2.cursor()
            dcur.execute( "DROP DATABASE IF EXISTS " + testdb )
            check( "T31 DROP DATABASE cleanup", c2.errorCode() == 0 )
            dcur.close()
            c2.close()
        }

        Console.println( "===== MysqlTest end PASS=" + s_pass.toString() + " FAIL=" + s_fail.toString() + " =====" )
    }
}
