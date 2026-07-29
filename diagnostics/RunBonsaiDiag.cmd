@echo off
REM ============================================================================
REM  RunBonsaiDiag.cmd -- one-click Bonsai crash diagnostics for the rig.
REM
REM  Double-click this file. It self-elevates (UAC prompt), registers WER
REM  LocalDumps so the NEXT Bonsai crash writes a full-heap dump to C:\CrashDumps,
REM  then shows the current status and searches for any existing dumps.
REM
REM  The memory sampler is separate (no admin needed) -- see the note printed at
REM  the end, or just run:  Sample.cmd
REM ============================================================================
setlocal
set "PS=%~dp0BonsaiCrashDiag.ps1"

REM --- self-elevate if not already admin ---
net session >nul 2>&1
if %errorlevel% NEQ 0 (
  echo Requesting administrator elevation...
  powershell -NoProfile -Command "Start-Process -FilePath '%~f0' -Verb RunAs"
  exit /b
)

echo Running Bonsai crash diagnostics (elevated)...
echo.
powershell -NoProfile -ExecutionPolicy Bypass -File "%PS%" -Setup -Status -Search

echo.
echo ============================================================================
echo  WER is now set to capture the next crash into C:\CrashDumps.
echo  When you start the rig run, ALSO start the memory sampler in its own window:
echo.
echo      "%~dp0Sample.cmd"
echo.
echo  After the next crash, copy C:\CrashDumps\Bonsai.exe.*.dmp (and bonsai_mem.csv)
echo  back to the dev machine for analysis.
echo ============================================================================
echo.
pause
