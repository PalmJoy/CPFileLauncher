@echo off
call "%~dp0CPFileLauncher\build-release.bat" %*
exit /b %ERRORLEVEL%
