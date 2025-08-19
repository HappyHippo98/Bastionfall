@echo off
setlocal enabledelayedexpansion

set OUTPUT=AllScripts.txt
if exist %OUTPUT% del %OUTPUT%

echo Sammle alle .cs-Dateien...

for /r %%F in (*.cs) do (
    set RELPATH=%%F
    set RELPATH=!RELPATH:%cd%\=!
    echo ===== FILE: !RELPATH! ===== >> %OUTPUT%
    type "%%F" >> %OUTPUT%
    echo. >> %OUTPUT%
    echo. >> %OUTPUT%
)

echo Fertig! Alles gesammelt in %OUTPUT%
pause
