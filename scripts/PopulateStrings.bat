@echo off
pushd "%~dp0"
set PROJECT_DIR=%1
set STBL_PREFIX=..\%PROJECT_DIR%\strings\ExpandedHouseholdStaff
set MAIN_LANG=ENG_US
for %%i in (ENG_US CHS_CN CHT_CN CZE_CZ DAN_DK DUT_NL FIN_FI FRE_FR GER_DE GRE_GR HUN_HU ITA_IT JPN_JP KOR_KR NOR_NO POL_PL POR_PT POR_BR RUS_RU SPA_ES SPA_MX SWE_SE THA_TH) do (
    if exist %STBL_PREFIX%_%%i.yaml (
        ..\tools\STBLize+\STBLize+.exe %STBL_PREFIX%_%MAIN_LANG%.yaml -a %STBL_PREFIX%_%%i.yaml
    ) else (
        copy %STBL_PREFIX%_%MAIN_LANG%.yaml %STBL_PREFIX%_%%i.yaml
    )
)
popd
