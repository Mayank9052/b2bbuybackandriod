@echo off
setlocal

set "FLUTTER_ROOT=C:\src\flutter"
set "PATH=C:\Program Files\Git\cmd;%FLUTTER_ROOT%\bin;%PATH%"

if not exist "%FLUTTER_ROOT%\bin\flutter.bat" (
  echo Flutter SDK was not found at %FLUTTER_ROOT%.
  exit /b 1
)

call "%FLUTTER_ROOT%\bin\flutter.bat" %*
exit /b %ERRORLEVEL%
