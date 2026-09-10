import Std;
import SLMysql;

# =========================================================================
# MysqlApiTest —— SLMysql API 细节测试（连接真实 MySQL 服务器）
#
# 覆盖：
#   A) 参数绑定（? 占位符）与转义（引号 / 反斜杠 / 中文）
#   B) SQL NULL 的写入与读取（isNull / getString）
#   C) fetchone / fetchmany / fetchall 三种取行方式
#   D) 事务：rollback 丢弃、commit 落库
#   E) 错误处理：错误 SQL 的 errorCode / errorMessage
#   F) Mysql.escape / Mysql.literal 的转义结果
#
# SLMysql 的类都在 namespace SL 下，故统一用 SL.xxx 全名访问。
# =========================================================================
class MysqlApiTest
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

    static fun()
    {
        Console.println( "" )
        Console.println( "===== MysqlApiTest start =====" )

        string testdb = global.mysqlTestDb

        # ── 准备干净的测试环境 ───────────────────────────────────
        SL.MysqlConnection root = SL.Mysql.connect( global.mysqlHost, global.mysqlUser, global.mysqlPassword, global.mysqlDatabase, global.mysqlPort, global.mysqlCharset )
        if root == null
        {
            Console.println( "connect failed, skip MysqlApiTest" )
            ret
        }

        SL.MysqlCursor rc = root.cursor()
        rc.execute( "DROP DATABASE IF EXISTS " + testdb )
        rc.execute( "CREATE DATABASE " + testdb + " DEFAULT CHARACTER SET utf8mb4" )
        check( "A01 CREATE DATABASE " + testdb, root.errorCode() == 0 )
        rc.close()
        root.close()

        SL.MysqlConnection conn = SL.Mysql.connect( global.mysqlHost, global.mysqlUser, global.mysqlPassword, testdb, global.mysqlPort, global.mysqlCharset )
        bool okConn = false
        if conn == null
        {
            okConn = false
        }
        else
        {
            okConn = true
        }
        check( "A02 connect to test db", okConn )
        if okConn == false
        {
            Console.println( "connect test db failed, skip rest" )
            ret
        }

        SL.MysqlCursor cur = conn.cursor()
        cur.execute( "CREATE TABLE t_api ( id INT NOT NULL AUTO_INCREMENT, title VARCHAR(255), memo VARCHAR(255) NULL, PRIMARY KEY (id) ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4" )
        check( "A03 CREATE TABLE t_api", conn.errorCode() == 0 )

        # ── A. 参数绑定 + 转义 ───────────────────────────────────
        string tricky = "O'Brien [q] % _ chinese-中文"
        cur.execute( "INSERT INTO t_api (title, memo) VALUES (?, ?)", Tuple( tricky, "memo-1" ) )
        check( "A04 INSERT with special chars", conn.errorCode() == 0 && conn.affectedRows() == 1 )

        Int64 trickyId = conn.insertId()
        cur.execute( "SELECT title FROM t_api WHERE id = ?", Tuple( trickyId ) )
        SL.MysqlRow trow = cur.fetchone()
        bool trickyOk = false
        if trow == null
        {
            trickyOk = false
        }
        else
        {
            trickyOk = trow.getString( 0 ) == tricky
        }
        check( "A05 special chars round trip", trickyOk )
        Console.println( "  tricky = " + tricky )
        if trow == null
        {
            Console.println( "  read   = <null row>" )
        }
        else
        {
            Console.println( "  read   = " + trow.getString( 0 ) )
        }

        # ── B. NULL 写入 / 读取 ──────────────────────────────────
        cur.execute( "INSERT INTO t_api (title, memo) VALUES (?, NULL)", Tuple( "null-memo" ) )
        check( "A06 INSERT NULL ok", conn.errorCode() == 0 )

        cur.execute( "SELECT title, memo FROM t_api WHERE title = 'null-memo'" )
        SL.MysqlRow nr = cur.fetchone()
        bool nullOk = false
        bool emptyOk = false
        bool titleOk = false
        if nr == null
        {
            nullOk = false
        }
        else
        {
            nullOk = nr.isNull( 1 )
            emptyOk = nr.getString( 1 ) == ""
            titleOk = nr.isNull( 0 ) == false
        }
        check( "A07 memo is SQL NULL", nullOk )
        check( "A08 NULL column getString == empty", emptyOk )
        check( "A09 non-null column isNull == false", titleOk )

        # ── C. 三种取行方式 ──────────────────────────────────────
        for i = 0, i < 5, i++
        {
            cur.execute( "INSERT INTO t_api (title, memo) VALUES (?, ?)", Tuple( "row-" + i.toString(), i.toString() ) )
        }
        check( "A10 insert 5 rows", conn.errorCode() == 0 )

        cur.execute( "SELECT id, title, memo FROM t_api ORDER BY id" )
        SL.MysqlRow one = cur.fetchone()
        bool oneOk = false
        if one == null
        {
            oneOk = false
        }
        else
        {
            oneOk = one.length == 3
        }
        check( "A11 fetchone first row", oneOk )
        if oneOk
        {
            Console.println( "  fetchone = " + one.toString() )
        }

        cur.execute( "SELECT id, title, memo FROM t_api ORDER BY id" )
        var many = cur.fetchmany( 2 )
        check( "A12 fetchmany(2) gets 2 rows", many.length == 2 )

        cur.execute( "SELECT id, title, memo FROM t_api ORDER BY id" )
        var all = cur.fetchall()
        check( "A13 fetchall gets all 7 rows", all.length == 7 )

        cur.execute( "SELECT id FROM t_api WHERE 1 = 0" )
        var emptyList = cur.fetchall()
        check( "A14 empty result fetchall -> 0 rows", emptyList.length == 0 )
        check( "A15 empty result columnCount == 1", cur.columnCount() == 1 )

        # ── D. 事务 ──────────────────────────────────────────────
        check( "A16 autocommit(false)", conn.autocommit( false ) )

        cur.execute( "INSERT INTO t_api (title, memo) VALUES (?, ?)", Tuple( "rollback-me", "x" ) )
        conn.rollback()
        cur.execute( "SELECT COUNT(*) FROM t_api WHERE title = 'rollback-me'" )
        SL.MysqlRow rbRow = cur.fetchone()
        bool rbOk = false
        if rbRow == null
        {
            rbOk = false
        }
        else
        {
            rbOk = rbRow.getString( 0 ) == "0"
        }
        check( "A17 rollback discards insert", rbOk )

        cur.execute( "INSERT INTO t_api (title, memo) VALUES (?, ?)", Tuple( "commit-me", "x" ) )
        conn.commit()
        cur.execute( "SELECT COUNT(*) FROM t_api WHERE title = 'commit-me'" )
        SL.MysqlRow cmRow = cur.fetchone()
        bool cmOk = false
        if cmRow == null
        {
            cmOk = false
        }
        else
        {
            cmOk = cmRow.getString( 0 ) == "1"
        }
        check( "A18 commit persists insert", cmOk )

        conn.autocommit( true )

        # ── E. 错误处理 ──────────────────────────────────────────
        cur.execute( "SELECT * FROM no_such_table_at_all" )
        Int32 errCode = conn.errorCode()
        Console.println( "  error = " + errCode.toString() + " : " + conn.errorMessage() )
        check( "A19 error SQL errorCode != 0", errCode != 0 )
        check( "A20 error SQL errorMessage not empty", conn.errorMessage() != "" )

        cur.execute( "SELECT 1" )
        SL.MysqlRow pingRow = cur.fetchone()
        bool pingOk = false
        if pingRow == null
        {
            pingOk = false
        }
        else
        {
            pingOk = pingRow.getString( 0 ) == "1"
        }
        check( "A21 connection still usable after error", pingOk )

        # ── F. escape / literal ──────────────────────────────────
        string esc = SL.Mysql.escape( conn.handle, "a'b" )
        Console.println( "  escape(a'b) = " + esc )
        check( "A22 escape handles quote", esc != "a'b" )

        string lit = SL.Mysql.literal( conn.handle, "xy" )
        check( "A23 literal string quoted", lit == "'xy'" )

        string litNull = SL.Mysql.literal( conn.handle, null )
        check( "A24 literal(null) == NULL", litNull == "NULL" )

        # ── G. 清理 ──────────────────────────────────────────────
        cur.execute( "DROP TABLE IF EXISTS t_api" )
        check( "A25 DROP TABLE t_api", conn.errorCode() == 0 )
        cur.close()
        conn.close()

        SL.MysqlConnection cc = SL.Mysql.connect( global.mysqlHost, global.mysqlUser, global.mysqlPassword, global.mysqlDatabase, global.mysqlPort, global.mysqlCharset )
        if cc != null
        {
            SL.MysqlCursor dc = cc.cursor()
            dc.execute( "DROP DATABASE IF EXISTS " + testdb )
            check( "A26 DROP DATABASE cleanup", cc.errorCode() == 0 )
            dc.close()
            cc.close()
        }

        Console.println( "===== MysqlApiTest end PASS=" + s_pass.toString() + " FAIL=" + s_fail.toString() + " =====" )
    }
}
