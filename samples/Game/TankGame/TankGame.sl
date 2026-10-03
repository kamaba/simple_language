# 坦克大战（实时输入版）
#
# 玩法：
#   w / s / a / d 或 方向键        移动坦克并转向
#   f 或 空格                       朝当前方向开火
#   q                               退出游戏
# 目标：消灭全部 6 个红色敌人坦克即获胜；被敌人子弹打中 3 次则失败。
#
# 特性（依赖 Std.Console 补全的接口）：
#   - 实时输入：Console.keyAvailable() + Console.readKeyDirect()，无需回车
#   - 大地图：44 x 24，带边框与砖墙障碍
#   - 坦克形状：3x3 ASCII 坦克，炮管随朝向变化
#   - 颜色：玩家绿色、敌人红色、子弹黄色/红色、墙体灰色
#   - 双向子弹：玩家与敌人都能开火，子弹每帧移动 2 格
#   - 无闪烁刷新：光标定位重绘（setCursorPosition），不整屏清屏
#
# 语法注意（本语言特性）：
#   - 类内访问自己的字段/方法都要加 this.
#   - 编译日志里的 "this is not allowed in input parameter expression"
#     是 Info 级提示（Core 标准库同样产生），不影响编译与运行

import Std;

TankGame
{
    # ---- 地图尺寸 ----
    Int32 width = 44
    Int32 height = 24

    # ---- 颜色（16 色控制台色号）----
    Int32 colorPlayer = 10
    Int32 colorEnemy = 12
    Int32 colorWall = 8
    Int32 colorBullet = 14
    Int32 colorText = 11

    # ---- 玩家坦克 ----
    Int32 px = 22
    Int32 py = 19
    Int32 dir = 0            # 朝向：0=上 1=下 2=左 3=右
    Int32 lives = 3
    Int32 fireCooldown = 0

    # ---- 游戏状态 ----
    Int32 score = 0
    bool running = true
    Int32 frame = 0

    # ---- 敌人（固定 6 个）：位置 / 朝向 / 存活 ----
    Int32 enemyCount = 6
    Int32[] ex = Array<Int32>.create(6)
    Int32[] ey = Array<Int32>.create(6)
    Int32[] edir = Array<Int32>.create(6)
    Int32[] ealive = Array<Int32>.create(6)

    # ---- 子弹池（玩家+敌人共用 24 发）----
    # bowner: 0=玩家 1=敌人
    Int32 maxBullets = 24
    Int32[] bx = Array<Int32>.create(24)
    Int32[] by = Array<Int32>.create(24)
    Int32[] bdir = Array<Int32>.create(24)
    Int32[] bowner = Array<Int32>.create(24)
    Int32[] bactive = Array<Int32>.create(24)

    # ---- 障碍墙（并行数组）----
    Int32[] wx = Array<Int32>.create(64)
    Int32[] wy = Array<Int32>.create(64)
    Int32 wallCount = 0

    # ---- 随机数 ----
    Random rng = new()

    # ---- 初始化 ----
    override _init_()
    {
        # 敌人布置在地图上半区
        this.ex[0] = 6
        this.ey[0] = 4
        this.edir[0] = 1
        this.ex[1] = 14
        this.ey[1] = 3
        this.edir[1] = 1
        this.ex[2] = 22
        this.ey[2] = 6
        this.edir[2] = 1
        this.ex[3] = 30
        this.ey[3] = 3
        this.edir[3] = 1
        this.ex[4] = 37
        this.ey[4] = 5
        this.edir[4] = 1
        this.ex[5] = 26
        this.ey[5] = 8
        this.edir[5] = 2

        Int32 i = 0
        while i < this.enemyCount
        {
            this.ealive[i] = 1
            i = i + 1
        }

        # 障碍墙：几组砖墙块
        this.addWallRect(7, 9, 12, 10)
        this.addWallRect(31, 9, 36, 10)
        this.addWallRect(19, 13, 24, 14)
        this.addWallRect(3, 4, 4, 7)
        this.addWallRect(39, 4, 40, 7)
        this.addWallRect(15, 17, 16, 18)
        this.addWallRect(27, 17, 28, 18)

        i = 0
        while i < this.maxBullets
        {
            this.bactive[i] = 0
            i = i + 1
        }
        ret
    }

    # 添加一堵矩形墙（闭区间）
    # 注意：数组下标不能直接写 this.wallCount（Front 限制），先取到局部变量
    void addWallRect(Int32 x0, Int32 y0, Int32 x1, Int32 y1)
    {
        Int32 y = y0
        while y <= y1
        {
            Int32 x = x0
            while x <= x1
            {
                Int32 wc = this.wallCount
                this.wx[wc] = x
                this.wy[wc] = y
                this.wallCount = wc + 1
                x = x + 1
            }
            y = y + 1
        }
        ret
    }

    # ---- 几何查询 ----

    # 边界外圈也算墙
    bool isBlocked(Int32 x, Int32 y)
    {
        if x <= 0 || x >= this.width - 1 || y <= 0 || y >= this.height - 1
        {
            ret true
        }
        Int32 i = 0
        while i < this.wallCount
        {
            if this.wx[i] == x && this.wy[i] == y { ret true }
            i = i + 1
        }
        ret false
    }

    # (x,y) 是否有存活的敌人
    bool isEnemyAt(Int32 x, Int32 y)
    {
        Int32 i = 0
        while i < this.enemyCount
        {
            if this.ealive[i] == 1 && this.ex[i] == x && this.ey[i] == y
            {
                ret true
            }
            i = i + 1
        }
        ret false
    }

    # 击中 (x,y) 处的敌人（若有）
    void killEnemyAt(Int32 x, Int32 y)
    {
        Int32 i = 0
        while i < this.enemyCount
        {
            if this.ealive[i] == 1 && this.ex[i] == x && this.ey[i] == y
            {
                this.ealive[i] = 0
                this.score = this.score + 10
                ret
            }
            i = i + 1
        }
        ret
    }

    # 剩余敌人数
    Int32 aliveEnemies()
    {
        Int32 n = 0
        Int32 i = 0
        while i < this.enemyCount
        {
            if this.ealive[i] == 1 { n = n + 1 }
            i = i + 1
        }
        ret n
    }

    # ---- 绘制 ----

    # 画一辆 3x3 坦克：中心 (x,y)，炮管随朝向 d
    # body 是车身字符，barrel 是炮管字符，color 是颜色号
    void drawTank(Int32 x, Int32 y, Int32 d, string body, string barrel, Int32 color)
    {
        string r0 = ""
        string r1 = ""
        string r2 = ""
        string bb = body + body + body

        if d == 0
        {
            r0 = " " + barrel + " "
            r1 = bb
            r2 = bb
        }
        elif d == 1
        {
            r0 = bb
            r1 = bb
            r2 = " " + barrel + " "
        }
        elif d == 2
        {
            r0 = barrel + "  "
            r1 = bb
            r2 = barrel + "  "
        }
        else
        {
            r0 = "  " + barrel
            r1 = bb
            r2 = "  " + barrel
        }

        Console.setForegroundColor(color)
        # 地图坐标 -> 屏幕坐标：屏幕 y 需加 2（顶部两行是 HUD）
        Console.setCursorPosition(x - 1, y - 1 + 2)
        Console.write(r0)
        Console.setCursorPosition(x - 1, y + 2)
        Console.write(r1)
        Console.setCursorPosition(x - 1, y + 1 + 2)
        Console.write(r2)
        ret
    }

    # 画一颗子弹
    void drawBullet(Int32 x, Int32 y, Int32 owner)
    {
        if owner == 0
        {
            Console.setForegroundColor(this.colorBullet)
            Console.setCursorPosition(x, y + 2)
            Console.write("*")
        }
        else
        {
            Console.setForegroundColor(this.colorEnemy)
            Console.setCursorPosition(x, y + 2)
            Console.write("x")
        }
        ret
    }

    # 整帧重绘（光标定位覆盖旧画面，不清屏防闪烁）
    void draw()
    {
        # HUD 第 0 行
        Console.setCursorPosition(0, 0)
        Console.setForegroundColor(this.colorText)
        Console.write("坦克大战  ")
        Console.setForegroundColor(14)
        Console.write("分数: " + this.score + "    生命: " + this.lives + "    剩余敌人: " + this.aliveEnemies() + "        ")

        # HUD 第 1 行
        Console.setCursorPosition(0, 1)
        Console.setForegroundColor(7)
        Console.write("w/a/s/d 或方向键移动    f/空格 开火    q 退出            ")

        # 地图（第 2 行起）
        Console.setForegroundColor(this.colorWall)
        Int32 y = 0
        while y < this.height
        {
            string line = ""
            Int32 x = 0
            while x < this.width
            {
                if x == 0 || x == this.width - 1 || y == 0 || y == this.height - 1
                {
                    line = line + "#"
                }
                elif this.isBlocked(x, y)
                {
                    line = line + "#"
                }
                else
                {
                    line = line + " "
                }
                x = x + 1
            }
            Console.setCursorPosition(0, y + 2)
            Console.write(line)
            y = y + 1
        }

        # 敌人
        Int32 i = 0
        while i < this.enemyCount
        {
            if this.ealive[i] == 1
            {
                this.drawTank(this.ex[i], this.ey[i], this.edir[i], "E", "e", this.colorEnemy)
            }
            i = i + 1
        }

        # 玩家
        this.drawTank(this.px, this.py, this.dir, "#", this.barrelChar(this.dir), this.colorPlayer)

        # 子弹
        i = 0
        while i < this.maxBullets
        {
            if this.bactive[i] == 1
            {
                this.drawBullet(this.bx[i], this.by[i], this.bowner[i])
            }
            i = i + 1
        }
        ret
    }

    # 玩家炮管字符（随朝向）
    string barrelChar(Int32 d)
    {
        if d == 0 { ret "^" }
        elif d == 1 { ret "v" }
        elif d == 2 { ret "<" }
        ret ">"
    }

    # ---- 移动与开火 ----

    # 玩家尝试移动到 (nx,ny)，并设定新朝向
    void tryMove(Int32 nx, Int32 ny, Int32 newDir)
    {
        this.dir = newDir
        if this.isBlocked(nx, ny) { ret }
        if this.isEnemyAt(nx, ny) { ret }
        if nx < 1 || nx > this.width - 2 || ny < 1 || ny > this.height - 2 { ret }
        this.px = nx
        this.py = ny
        ret
    }

    # 找一个空闲子弹槽位，没有则返回 -1
    Int32 freeBulletSlot()
    {
        Int32 i = 0
        while i < this.maxBullets
        {
            if this.bactive[i] == 0 { ret i }
            i = i + 1
        }
        ret -1
    }

    # 发射一颗子弹（起点已在发射者前方）
    void spawnBullet(Int32 owner, Int32 x, Int32 y, Int32 d)
    {
        Int32 slot = this.freeBulletSlot()
        if slot < 0 { ret }
        this.bx[slot] = x
        this.by[slot] = y
        this.bdir[slot] = d
        this.bowner[slot] = owner
        this.bactive[slot] = 1
        ret
    }

    # 玩家开火：朝当前朝向
    void playerFire()
    {
        if this.fireCooldown > 0 { ret }
        Int32 fx = this.px
        Int32 fy = this.py
        if this.dir == 0 { fy = fy - 1 }
        elif this.dir == 1 { fy = fy + 1 }
        elif this.dir == 2 { fx = fx - 1 }
        else { fx = fx + 1 }

        if this.isBlocked(fx, fy) { ret }
        this.spawnBullet(0, fx, fy, this.dir)
        this.fireCooldown = 8
        ret
    }

    # 敌人开火
    void enemyFire(Int32 e)
    {
        Int32 fx = this.ex[e]
        Int32 fy = this.ey[e]
        Int32 d = this.edir[e]
        if d == 0 { fy = fy - 1 }
        elif d == 1 { fy = fy + 1 }
        elif d == 2 { fx = fx - 1 }
        else { fx = fx + 1 }

        if this.isBlocked(fx, fy) { ret }
        this.spawnBullet(1, fx, fy, d)
        ret
    }

    # 玩家被击中
    void playerHit()
    {
        this.lives = this.lives - 1
        if this.lives <= 0
        {
            this.running = false
            ret
        }
        # 回到出生点
        this.px = 22
        this.py = 19
        this.dir = 0
        ret
    }

    # ---- 子弹更新（每帧移动 2 格，逐格判碰撞）----
    void updateBullets()
    {
        Int32 i = 0
        while i < this.maxBullets
        {
            if this.bactive[i] == 1
            {
                Int32 stop = 0
                Int32 step = 0
                while step < 2 && stop == 0
                {
                    # 前进一格
                    if this.bdir[i] == 0 { this.by[i] = this.by[i] - 1 }
                    elif this.bdir[i] == 1 { this.by[i] = this.by[i] + 1 }
                    elif this.bdir[i] == 2 { this.bx[i] = this.bx[i] - 1 }
                    else { this.bx[i] = this.bx[i] + 1 }

                    Int32 cx = this.bx[i]
                    Int32 cy = this.by[i]

                    # 撞墙/边界
                    if cx <= 0 || cx >= this.width - 1 || cy <= 0 || cy >= this.height - 1
                    {
                        this.bactive[i] = 0
                        stop = 1
                    }
                    elif this.isBlocked(cx, cy)
                    {
                        this.bactive[i] = 0
                        stop = 1
                    }
                    else
                    {
                        if this.bowner[i] == 0
                        {
                            # 玩家子弹：打敌人
                            if this.isEnemyAt(cx, cy)
                            {
                                this.killEnemyAt(cx, cy)
                                this.bactive[i] = 0
                                stop = 1
                            }
                        }
                        else
                        {
                            # 敌人子弹：打玩家
                            if cx == this.px && cy == this.py
                            {
                                this.playerHit()
                                this.bactive[i] = 0
                                stop = 1
                            }
                        }
                    }
                    step = step + 1
                }
            }
            i = i + 1
        }
        ret
    }

    # ---- 敌人 AI：定时移动 / 随机转向 / 随机开火 ----
    void updateEnemies()
    {
        Int32 i = 0
        while i < this.enemyCount
        {
            if this.ealive[i] == 1
            {
                # 每个敌人错峰行动：6 帧一动
                if this.frame % 6 == i % 6
                {
                    # 小概率换方向
                    if this.rng.nextInt(5) == 0
                    {
                        this.edir[i] = this.rng.nextInt(4)
                    }

                    # 朝当前方向走一步，堵住就换向
                    Int32 nx = this.ex[i]
                    Int32 ny = this.ey[i]
                    if this.edir[i] == 0 { ny = ny - 1 }
                    elif this.edir[i] == 1 { ny = ny + 1 }
                    elif this.edir[i] == 2 { nx = nx - 1 }
                    else { nx = nx + 1 }

                    if this.isBlocked(nx, ny) || this.isEnemyAt(nx, ny) || (nx == this.px && ny == this.py)
                    {
                        this.edir[i] = this.rng.nextInt(4)
                    }
                    else
                    {
                        this.ex[i] = nx
                        this.ey[i] = ny
                    }

                    # 概率开火
                    if this.rng.nextInt(6) == 0
                    {
                        this.enemyFire(i)
                    }
                }
            }
            i = i + 1
        }
        ret
    }

    # ---- 胜负判定 ----
    void checkWin()
    {
        if this.aliveEnemies() == 0
        {
            this.running = false
        }
        ret
    }

    # ---- 输入处理（实时，非阻塞）----
    void handleKey(string k)
    {
        if k == "w" || k == "up" { this.tryMove(this.px, this.py - 1, 0) }
        elif k == "s" || k == "down" { this.tryMove(this.px, this.py + 1, 1) }
        elif k == "a" || k == "left" { this.tryMove(this.px - 1, this.py, 2) }
        elif k == "d" || k == "right" { this.tryMove(this.px + 1, this.py, 3) }
        elif k == "f" || k == " " { this.playerFire() }
        elif k == "q" { this.running = false }
        ret
    }

    # ---- 主循环 ----
    void run()
    {
        Console.hideCursor()
        Console.clear()
        Console.setCursorPosition(0, this.height + 3)
        Console.resetColor()
        SystemPrintln("坦克大战开始！w/a/s/d 移动，f/空格 开火，q 退出。")

        while this.running == true
        {
            # 1) 非阻塞读入本轮所有按键
            while Console.keyAvailable() == true
            {
                string k = Console.readKeyDirect()
                this.handleKey(k)
            }

            # 2) 玩家开火冷却
            if this.fireCooldown > 0
            {
                this.fireCooldown = this.fireCooldown - 1
            }

            # 3) 敌人 AI
            this.updateEnemies()

            # 4) 子弹推进与碰撞
            this.updateBullets()

            # 5) 胜负
            this.checkWin()

            # 6) 重绘 + 帧间隔
            this.draw()
            Console.sleep(60)
            this.frame = this.frame + 1
        }

        # 收尾
        Console.resetColor()
        Console.setCursorPosition(0, this.height + 3)
        Console.showCursor()
        if this.lives <= 0
        {
            SystemPrintln("游戏结束，你被消灭了。最终分数: " + this.score)
        }
        else
        {
            SystemPrintln("胜利！全部敌人被消灭。最终分数: " + this.score)
        }
        ret
    }
}
