import Std;

Sqlite3Test
{
    # 测试连接数据库
    static testConnect()
    {
        Console.println("===== Sqlite3Test.testConnect =====")
        var conn = DB.Sqlite3.connect("Resources/ttest")
        if conn == null
        {
            Console.println("[FAIL] 连接数据库失败")
            ret
        }
        Console.println("[PASS] 连接数据库成功")
        conn.close()
    }

    # 测试插入数据（带参数绑定）
    static testInsert()
    {
        Console.println("===== Sqlite3Test.testInsert =====")
        var conn = DB.Sqlite3.connect("Resources/ttest")
        if conn == null
        {
            Console.println("[FAIL] 连接数据库失败")
            ret
        }

        var cursor = conn.cursor()

        # 清空表数据
        cursor.execute("DELETE FROM test1")
        Console.println("清空表数据，影响行数: " + cursor.changes())

        # 插入数据（使用 Tuple 绑定参数）
        cursor.execute("INSERT INTO test1 (uid, name) VALUES (?, ?)", Tuple(1, "alice"))
        Console.println("插入 alice，rowid = " + cursor.lastInsertRowid())

        cursor.execute("INSERT INTO test1 (uid, name) VALUES (?, ?)", Tuple(2, "bob"))
        Console.println("插入 bob，rowid = " + cursor.lastInsertRowid())

        cursor.execute("INSERT INTO test1 (uid, name) VALUES (?, ?)", Tuple(3, "charlie"))
        Console.println("插入 charlie，rowid = " + cursor.lastInsertRowid())

        Console.println("[PASS] 插入 3 条数据")
        cursor.close()
        conn.close()
    }

    # 测试查询全部数据（fetchall）
    static testFetchAll()
    {
        Console.println("===== Sqlite3Test.testFetchAll =====")
        var conn = DB.Sqlite3.connect("Resources/ttest")
        if conn == null
        {
            Console.println("[FAIL] 连接数据库失败")
            ret
        }

        var cursor = conn.cursor()
        cursor.execute("SELECT * FROM test1")
        var rows = cursor.fetchall()

        Console.println("查询到 " + rows.length + " 行数据:")
        for i = 0, i < rows.length, i++
        {
            DB.Row row = rows._getItem_(i) as DB.Row
            Console.println("  uid=" + row._getItem_(0).toString() + ", name=" + row._getItem_(1))
        }

        if rows.length > 0
        {
            Console.println("[PASS] fetchall 查询成功")
        }
        else
        {
            Console.println("[FAIL] fetchall 无数据")
        }

        cursor.close()
        conn.close()
    }

    # 测试逐行查询（fetchone）
    static testFetchOne()
    {
        Console.println("===== Sqlite3Test.testFetchOne =====")
        var conn = DB.Sqlite3.connect("Resources/ttest")
        if conn == null
        {
            Console.println("[FAIL] 连接数据库失败")
            ret
        }

        var cursor = conn.cursor()
        cursor.execute("SELECT * FROM test1 ORDER BY uid")

        Console.println("逐行读取:")
        int count = 0
        var row = cursor.fetchone()
        while row != null
        {
            Console.println("  第 " + count + " 行: uid=" + row._getItem_(0).toString() + ", name=" + row._getItem_(1))
            count++
            row = cursor.fetchone()
        }

        Console.println("共读取 " + count + " 行")
        if count > 0
        {
            Console.println("[PASS] fetchone 查询成功")
        }
        else
        {
            Console.println("[FAIL] fetchone 无数据")
        }

        cursor.close()
        conn.close()
    }

    # 测试条件查询（带参数）
    static testWhereQuery()
    {
        Console.println("===== Sqlite3Test.testWhereQuery =====")
        var conn = DB.Sqlite3.connect("Resources/ttest")
        if conn == null
        {
            Console.println("[FAIL] 连接数据库失败")
            ret
        }

        var cursor = conn.cursor()

        # 条件查询：uid > 1
        cursor.execute("SELECT * FROM test1 WHERE uid > ?", Tuple(1))
        var rows = cursor.fetchall()

        Console.println("uid > 1 的结果（" + rows.length + " 行）:")
        for i = 0, i < rows.length, i++
        {
            DB.Row row = rows._getItem_(i) as DB.Row
            Console.println("  uid=" + row._getItem_(0).toString() + ", name=" + row._getItem_(1))
        }

        if rows.length > 0
        {
            Console.println("[PASS] 条件查询成功")
        }
        else
        {
            Console.println("[FAIL] 条件查询无数据")
        }

        cursor.close()
        conn.close()
    }

    # 测试更新和删除
    static testUpdateDelete()
    {
        Console.println("===== Sqlite3Test.testUpdateDelete =====")
        var conn = DB.Sqlite3.connect("Resources/ttest")
        if conn == null
        {
            Console.println("[FAIL] 连接数据库失败")
            ret
        }

        var cursor = conn.cursor()

        # 更新：把 alice 的名字改为 Alice
        cursor.execute("UPDATE test1 SET name = ? WHERE name = ?", Tuple("Alice", "alice"))
        Console.println("UPDATE 影响行数: " + cursor.changes())

        # 验证更新结果
        cursor.execute("SELECT name FROM test1 WHERE uid = ?", Tuple(1))
        var row = cursor.fetchone()
        if row != null
        {
            Console.println("更新后 uid=1 的 name = " + row._getItem_(0))
            if row._getItem_(0) == "Alice"
            {
                Console.println("[PASS] 更新成功")
            }
            else
            {
                Console.println("[FAIL] 更新后名称不正确")
            }
        }

        # 删除：删除 bob
        cursor.execute("DELETE FROM test1 WHERE name = ?", Tuple("bob"))
        Console.println("DELETE 影响行数: " + cursor.changes())

        # 验证删除结果
        cursor.execute("SELECT COUNT(*) FROM test1")
        var countRow = cursor.fetchone()
        if countRow != null
        {
            Console.println("删除后剩余行数: " + countRow._getItem_(0).toString())
        }

        cursor.close()
        conn.close()
    }

    # 测试 Command 对象（ADO.NET 风格）
    static testCommand()
    {
        Console.println("===== Sqlite3Test.testCommand =====")
        var conn = DB.Sqlite3.connect("Resources/ttest")
        if conn == null
        {
            Console.println("[FAIL] 连接数据库失败")
            ret
        }

        # 使用 Command 执行 DDL
        var cmd = conn.createCommand("CREATE TABLE IF NOT EXISTS test2 (id INTEGER PRIMARY KEY, value TEXT)")
        int rc = cmd.executeNonQuery()
        Console.println("CREATE TABLE 返回码: " + rc)

        # 使用 Command 插入数据
        var cmd2 = conn.createCommand("INSERT INTO test2 (id, value) VALUES (1, 'hello')")
        cmd2.executeNonQuery()
        Console.println("INSERT 通过 Command 完成")

        # 使用 Command 删除表
        var cmd3 = conn.createCommand("DROP TABLE test2")
        cmd3.executeNonQuery()
        Console.println("DROP TABLE 通过 Command 完成")

        Console.println("[PASS] Command 对象测试完成")
        conn.close()
    }

    # 测试 Connection.execute 便捷方法
    static testConnectionExecute()
    {
        Console.println("===== Sqlite3Test.testConnectionExecute =====")
        var conn = DB.Sqlite3.connect("Resources/ttest")
        if conn == null
        {
            Console.println("[FAIL] 连接数据库失败")
            ret
        }

        # 使用 conn.execute 便捷方法
        var cursor = conn.execute("SELECT * FROM test1")
        var rows = cursor.fetchall()
        Console.println("conn.execute 查询到 " + rows.length + " 行")

        # 使用 conn.execute 带参数
        var cursor2 = conn.execute("SELECT * FROM test1 WHERE uid = ?", Tuple(1))
        var row = cursor2.fetchone()
        if row != null
        {
            Console.println("conn.execute 带参数查询: uid=" + row._getItem_(0).toString() + ", name=" + row._getItem_(1))
        }

        Console.println("[PASS] Connection.execute 便捷方法测试完成")
        cursor.close()
        cursor2.close()
        conn.close()
    }

    static fun()
    {
        Console.println("===== Sqlite3Test =====")
        testConnect()
        testInsert()
        testFetchAll()
        testFetchOne()
        testWhereQuery()
        testUpdateDelete()
        testCommand()
        testConnectionExecute()
    }
}
