# 最简单的坦克游戏（cmd 回合式）
#
# 玩法：
#   w / s / a / d   上 / 下 / 左 / 右   移动坦克并转向
#   f               朝当前方向开火（打掉正前方第一个敌人）
#   q               退出游戏
# 目标：消灭全部 3 个敌人（E）即获胜。
# 说明：每回合在命令行重新打印整张地图，输入指令后按回车生效。

TankGame
{
    # ---- 地图尺寸 ----
    Int32 width = 16
    Int32 height = 12

    # ---- 玩家坦克 ----
    Int32 px = 8          # 玩家所在列
    Int32 py = 10         # 玩家所在行
    Int32 dir = 0         # 朝向：0=上 1=下 2=左 3=右

    # ---- 敌人（固定 3 个），eAlive 用 1 表示存活、0 表示被消灭 ----
    Int32[] ex = Array<Int32>.create(3)
    Int32[] ey = Array<Int32>.create(3)
    Int32[] eAlive = Array<Int32>.create(3)

    # ---- 障碍物（内部墙，固定 4 个）----
    Int32[] wx = Array<Int32>.create(4)
    Int32[] wy = Array<Int32>.create(4)

    # ---- 游戏状态 ----
    Int32 score = 0
    bool running = true

    # 构造函数：摆放敌人与障碍
    _init_()
    {
        ex[0] = 3;  ey[0] = 2;  eAlive[0] = 1
        ex[1] = 8;  ey[1] = 3;  eAlive[1] = 1
        ex[2] = 12; ey[2] = 2;  eAlive[2] = 1

        wx[0] = 5;  wy[0] = 5
        wx[1] = 6;  wy[1] = 5
        wx[2] = 10; wy[2] = 7
        wx[3] = 11; wy[3] = 7
    }

    # 根据朝向返回坦克的显示字符
    string tankDirChar(Int32 d)
    {
        if d == 0 { ret "^" }
        elif d == 1 { ret "v" }
        elif d == 2 { ret "<" }
        else { ret ">" }
    }

    # 判断 (x,y) 是否是墙（边界或障碍）
    bool isWall(Int32 x, Int32 y)
    {
        if x <= 0 || x >= width - 1 || y <= 0 || y >= height - 1
        {
            ret true
        }
        for i = 0, i < 4, i++
        {
            if wx[i] == x && wy[i] == y
            {
                ret true
            }
        }
        ret false
    }

    # 判断 (x,y) 是否有存活的敌人
    bool isEnemyAt(Int32 x, Int32 y)
    {
        for i = 0, i < 3, i++
        {
            if eAlive[i] == 1 && ex[i] == x && ey[i] == y
            {
                ret true
            }
        }
        ret false
    }

    # 用空行把旧画面推到屏幕上方（最简单的“清屏”）
    void clearScreen()
    {
        for i = 0, i < 30, i++
        {
            SystemPrintln("")
        }
        ret
    }

    # 绘制整张地图 + 状态栏
    void draw()
    {
        this.clearScreen()
        SystemPrintln("===== 最简单的坦克游戏 =====   分数:" + SystemConvertString(score))
        for y = 0, y < height, y++
        {
            string line = ""
            for x = 0, x < width, x++
            {
                if x == px && y == py
                {
                    line = line + this.tankDirChar(dir)
                }
                elif this.isEnemyAt(x, y)
                {
                    line = line + "E"
                }
                elif this.isWall(x, y)
                {
                    line = line + "#"
                }
                else
                {
                    line = line + "."
                }
            }
            SystemPrintln(line)
        }
        SystemPrintln("")
        SystemPrintln("w/a/s/d 移动    f 开火    q 退出")
        ret
    }

    # 尝试移动到 (nx,ny)，并设定新朝向
    void tryMove(Int32 nx, Int32 ny, Int32 newDir)
    {
        dir = newDir
        if this.isWall(nx, ny)
        {
            ret
        }
        if this.isEnemyAt(nx, ny)
        {
            # 直接碾过敌人并将其消灭
            for i = 0, i < 3, i++
            {
                if eAlive[i] == 1 && ex[i] == nx && ey[i] == ny
                {
                    eAlive[i] = 0
                    score = score + 1
                }
            }
            px = nx; py = ny
            ret
        }
        px = nx; py = ny
        ret
    }

    # 朝当前方向发射：沿直线寻找第一个墙或敌人
    void fire()
    {
        Int32 cx = px
        Int32 cy = py
        if dir == 0 { cy = cy - 1 }
        elif dir == 1 { cy = cy + 1 }
        elif dir == 2 { cx = cx - 1 }
        else { cx = cx + 1 }

        while cx > 0 && cx < width - 1 && cy > 0 && cy < height - 1
        {
            if this.isWall(cx, cy)
            {
                ret
            }
            if this.isEnemyAt(cx, cy)
            {
                for i = 0, i < 3, i++
                {
                    if eAlive[i] == 1 && ex[i] == cx && ey[i] == cy
                    {
                        eAlive[i] = 0
                        score = score + 1
                        SystemPrintln("命中敌人！分数:" + SystemConvertString(score))
                    }
                }
                ret
            }
            if dir == 0 { cy = cy - 1 }
            elif dir == 1 { cy = cy + 1 }
            elif dir == 2 { cx = cx - 1 }
            else { cx = cx + 1 }
        }
        ret
    }

    # 检查是否所有敌人都被消灭
    void checkWin()
    {
        bool allDead = true
        for i = 0, i < 3, i++
        {
            if eAlive[i] == 1
            {
                allDead = false
            }
        }
        if allDead == true
        {
            running = false
            SystemPrintln("胜利！全部敌人被消灭。最终分数:" + SystemConvertString(score))
        }
        ret
    }

    # 根据输入指令执行动作
    void handle(string cmd)
    {
        if cmd == "w" { this.tryMove(px, py - 1, 0) }
        elif cmd == "s" { this.tryMove(px, py + 1, 1) }
        elif cmd == "a" { this.tryMove(px - 1, py, 2) }
        elif cmd == "d" { this.tryMove(px + 1, py, 3) }
        elif cmd == "f" { this.fire() }
        elif cmd == "q" { running = false }
        else { SystemPrintln("未知指令:" + cmd) }
        ret
    }

    # 主循环
    void run()
    {
        SystemPrintln("欢迎来到最简单的坦克游戏！")
        SystemPrintln("w/a/s/d 移动，f 开火，q 退出。每次输入后按回车。")
        while running == true
        {
            this.draw()
            string cmd = SystemReadLine()
            this.handle(cmd)
            this.checkWin()
        }
        SystemPrintln("游戏结束，谢谢游玩！")
        ret
    }
}
