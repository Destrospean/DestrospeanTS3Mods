@echo off
pushd "%~dp0"
setlocal enabledelayedexpansion
set "filename=ts3buildtool-%modName%.log"
for /f "delims=" %%a in (%filename%) do (
    set "last_line=%%a"
)
set "path=!last_line:~52!"
..\tools\TuningResourceGenerator\TuningResourceGenerator.exe "!path!"
del %filename%
popd
