@echo off
setlocal ENABLEDELAYEDEXPANSION

REM ===============================
REM Unity Assets Snapshot (Tree + C#)
REM Run from inside the Assets folder
REM ===============================

set "ASSETS_DIR=%CD%"
set "OUT=%ASSETS_DIR%\allScripts.txt"

REM ---- Header ----
(
  echo # === Assets Snapshot ===
  echo # Created: %date% %time%
  echo # Assets: %ASSETS_DIR%
  echo # ------------------------------------
  echo.
  echo ===== DIRECTORY TREE (nur Ordner mit .cs^) =====
) > "%OUT%"

REM ---- Tree (ohne sort/findstr/PowerShell) ----
set "SEEN=;"
set "FOUND_CS=0"

for /r "%ASSETS_DIR%" %%F in (*.cs) do (
  set "FOUND_CS=1"
  set "dir=%%~dpF"
  set "rel=!dir:%ASSETS_DIR%\=!"
  if "!rel:~0,1!"=="\" set "rel=!rel:~1!"
  if "!rel:~-1!"=="\" set "rel=!rel:~0,-1!"
  set "key=Assets\!rel!"

  REM bereits gesehen?
  set "chk=!SEEN:;!key!;=;!"
  if "!chk!"=="!SEEN!" (
    >>"%OUT%" echo !key!
    set "SEEN=!SEEN!!key!;"
  )

  >>"%OUT%" echo   - %%~nxF
)

if "!FOUND_CS!"=="0" (
  >>"%OUT%" echo [no .cs files found]
)

>>"%OUT%" echo ===== END DIRECTORY TREE =====

REM ---- Alle .cs-Dateien inkl. Inhalt ----
(
  echo.
  echo ===== C# SOURCES UNDER Assets =====
) >> "%OUT%"

pushd "%ASSETS_DIR%" >nul
for /r %%F in (*.cs) do (
  set "rel=%%F"
  set "rel=!rel:%CD%\=!"
  if "!rel:~0,1!"=="\" set "rel=!rel:~1!"
  >> "%OUT%" echo.
  >> "%OUT%" echo ---------- FILE: Assets\!rel! ----------
  type "%%F" >> "%OUT%"
)
popd >nul

echo Wrote "%OUT%"
endlocal
