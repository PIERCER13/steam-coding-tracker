@echo off
set CSC=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe
"%CSC%" /nologo /target:winexe /platform:x86 /optimize+ /reference:Steam4NET.dll /out:piercer_steam_idle_worker.exe IdleWorker.cs
if %ERRORLEVEL% NEQ 0 (
  echo [ERRO] Falha ao compilar worker.
  pause
  exit /b 1
)
"%CSC%" /nologo /target:winexe /optimize+ /out:piercer_steam_coding_tracker.exe Program.cs
if %ERRORLEVEL% NEQ 0 (
  echo [ERRO] Falha ao compilar tracker.
  pause
  exit /b 1
)
echo [OK] Compilado: piercer_steam_idle_worker.exe + piercer_steam_coding_tracker.exe
