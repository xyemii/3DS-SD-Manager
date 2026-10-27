@echo off
setlocal
title 3DS SD Manager - build

rem Uses the C# compiler that ships with Windows (.NET Framework 4.x) - nothing to install.
set "CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not exist "%CSC%" set "CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe"
if not exist "%CSC%" (
  echo csc.exe not found. Is the .NET Framework 4.x installed?
  if /i not "%~1"=="nopause" pause
  exit /b 1
)

if not exist "%~dp0bin" mkdir "%~dp0bin"
echo Building bin\SD-Manager-3DS.exe ...
"%CSC%" /nologo /target:winexe /optimize+ /out:"%~dp0bin\SD-Manager-3DS.exe" /win32icon:"%~dp0assets\icon.ico" ^
  /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Web.Extensions.dll ^
  /reference:System.IO.Compression.dll /reference:System.IO.Compression.FileSystem.dll "%~dp0src\*.cs"
if errorlevel 1 (
  echo.
  echo Build failed - see the messages above.
  if /i not "%~1"=="nopause" pause
  exit /b 1
)
echo.
echo Done: bin\SD-Manager-3DS.exe
if /i not "%~1"=="nopause" pause
