# 远程 Linux 调试文档

> 用途：在远程 Linux 服务器上部署并运行 SimpleLanguage C VM（`linux-dist` 分发包）。
> 本机工具链：Windows + PuTTY（`plink`/`pscp`，安装于 `C:\Program Files\PuTTY\`）。
> 首次验证：2026-09-25，6 套件全绿（ALL SUITES OK）。

## 1. 服务器信息

| 项 | 值 |
|----|----|
| 地址 | `82.157.37.133:22`（SSH） |
| 用户 | `root` |
| 密码 | `Likecg00@123` |
| Host Key | `SHA256:W23PEm8t93yh/nb7TS0MLM5ZNxs2p5A/BcgZVbi84gM`（ssh-ed25519） |
| 架构 | x86_64 |
| 系统 | openEuler 系内核（`6.6.119-49.22.oc9.x86_64`），**glibc 2.38** |
| 配置 | 2 核 / 2GB 内存 / 磁盘 50G（余 41G） |
| 部署目录 | `/usr/project/cvm`（分发包解压于 `/usr/project/cvm/linux-dist`） |

⚠️ 密码为明文存放，仅限内部调试机使用；服务器对公网开放 22 端口，建议后续换密钥登录。

## 2. 常用命令（Windows 本机 PowerShell）

```powershell
# 公共参数（每次连接都要带）
$HK = '-hostkey SHA256:W23PEm8t93yh/nb7TS0MLM5ZNxs2p5A/BcgZVbi84gM'

# 执行远程命令
plink -ssh -batch $HK -pw Likecg00@123 root@82.157.37.133 "<命令>"

# 上传文件到服务器
pscp -batch $HK -pw Likecg00@123 <本地文件> root@82.157.37.133:/usr/project/cvm/

# 下载文件回本机
pscp -batch $HK -pw Likecg00@123 root@82.157.37.133:<远程文件> <本地目录>
```

**PowerShell 转义坑**：远程命令里的 shell 变量 `$?`/`$1` 等会被 PowerShell 抢先展开，须用反引号转义（`` echo RUN_EXIT=`$? ``）。

## 3. 分发包更新（重新部署）

```powershell
# 1) 本机打最新的分发包（详见根目录 AGENTS.md §5「Linux 独立分发包」）
wsl bash -c 'cd /mnt/d/project/lang && rm -rf linux-dist/build && tar czf linux-dist.tar.gz linux-dist/'

# 2) 上传（22MB，实测约 11MB/s）
pscp -batch $HK -pw Likecg00@123 d:\project\lang\linux-dist.tar.gz root@82.157.37.133:/usr/project/cvm/

# 3) 远程解压覆盖
plink -ssh -batch $HK -pw Likecg00@123 root@82.157.37.133 "cd /usr/project/cvm && tar xzf linux-dist.tar.gz && chmod +x linux-dist/run.sh linux-dist/run-tests.sh linux-dist/bin/csimple_lang"
```

## 4. 远程运行测试

```powershell
# 全部 6 套件回归（输出落 /usr/project/cvm/run-tests.log，终端只回传汇总）
plink -ssh -batch $HK -pw Likecg00@123 root@82.157.37.133 "cd /usr/project/cvm/linux-dist && ./run-tests.sh > /usr/project/cvm/run-tests.log 2>&1; tail -20 /usr/project/cvm/run-tests.log"

# 单模块运行（如 ProjectTest；不带参数会列出全部可用模块）
plink -ssh -batch $HK -pw Likecg00@123 root@82.157.37.133 "cd /usr/project/cvm/linux-dist && ./run.sh ProjectTest"
```

## 5. 实测记录

### 2026-09-25 首次部署（linux-dist 295MB / tar.gz 22MB）

| 套件 | 退出码 | 结果 | 备注 |
|------|--------|------|------|
| BaseTest | 0 | OK | MonitorTest 段 9 FAIL 与 Windows 侧一致（已知问题） |
| ExpendTest | 0 | OK | |
| MathTest | 0 | OK | 矩阵/FFI 段 FAIL 属预期（math_lib.dll 仅 Windows） |
| SpecialTest | 1 | OK（预期 1） | win64 插件被平台校验拦截，`SL_FORCE_RUN=1` 已内建于脚本 |
| BenchMark | 0 | OK | 31.6s（WSL 同机 16.9s，2 核云机慢一倍属正常） |
| NullFastTest | 0 | OK | |

**结论：ALL SUITES OK**。glibc 2.38 可正常运行本包 exe（构建环境 glibc 2.43，实际未用到更高版本符号）；若日后升级代码后远程报 `GLIBC_2.4x not found`，说明新代码引用了更高版本符号，需在远程机装 clang 重新编译 VM。

## 6. 排障速查

| 症状 | 处理 |
|------|------|
| `The host key is not cached` | 加 `-hostkey SHA256:W23PEm8t93yh/nb7TS0MLM5ZNxs2p5A/BcgZVbi84gM` |
| 连接卡住无输出 | 确认命令带了 `-batch`，不要用交互式管道喂 `echo y` |
| exe 报 `GLIBC_2.xx not found` | 远程 glibc 过旧，需在远程机重编（见 §5 结论） |
| `Permission denied` | 密码改动过则更新本文档 §1 与命令中的 `-pw` |
| 运行输出乱码/异常 | 先看 `/usr/project/cvm/linux-dist/build/logs/VM.txt`（VM 运行日志落 cwd） |
| 内存不足被 kill（OOM） | 机器仅 2GB，BenchMark 高峰内存占用大，勿与其他大进程并发 |
