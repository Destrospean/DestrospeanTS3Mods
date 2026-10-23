@echo off
pushd "%~dp0"
set projectDir=%1
del %projectDir%\resources\*.stbl
for %%i in (%projectDir%\strings\*.yaml) do (
    ..\tools\STBLize+\STBLize+.exe "%projectDir%\strings\%%i" -d %projectDir%\resources -nu
)
popd
