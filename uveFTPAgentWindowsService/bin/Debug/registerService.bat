@ECHO OFF

REM The following directory is for .NET 2.0
set DOTNETFX2=%SystemRoot%\Microsoft.NET\Framework\v2.0.50727
set PATH=%PATH%;%DOTNETFX2%

echo Installing FtpAgentWindowsService...
echo ---------------------------------------------------
InstallUtil /i FtpAgentWindowsService.exe /inifile=C:\works\UveIntegrator\UveIntegrator\UveIntegrator\bin\Debug\connecta.ini
echo ---------------------------------------------------
echo Done.

pause
