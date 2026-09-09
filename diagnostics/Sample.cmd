@echo off
REM ============================================================================
REM  Sample.cmd -- start the Bonsai memory sampler (no admin needed).
REM  Run this in its own window when you start the rig acquisition. It appends a
REM  row every 60 s to C:\CrashDumps\bonsai_mem.csv until you close it (Ctrl+C).
REM  Leave it running for the whole ~3h so we capture the ramp up to the crash.
REM ============================================================================
setlocal
set "PS=%~dp0BonsaiCrashDiag.ps1"
powershell -NoProfile -ExecutionPolicy Bypass -File "%PS%" -Sample
