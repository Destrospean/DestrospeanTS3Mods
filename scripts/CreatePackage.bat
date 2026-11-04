@echo off
pushd "%~dp0"

set modName=%1
set dllPath=%2
set projectDir=%3
set defaultPath=%4

set "dllPath=%dllPath:\=/%"
set "projectDir=%projectDir:\=/%"
set "defaultPath=%defaultPath:\=/%"

..\tools\TS3BuildTool\ts3buildtool.exe -modName=%modName% -dllPath=%dllPath% -projectDir=%projectDir% -defaultPath=%defaultPath% > ts3buildtool-%modName%.log

popd
