@echo off
setlocal enabledelayedexpansion
for %%i in (*.cs) do (
    set "filename=%%i"
    ren "!filename!" "!filename:.cs=.sl!"
)
echo 批量修改完成！
pause