@echo off
setlocal ENABLEDELAYEDEXPANSION
set path_=%1
set path_=%path:\=/%
cd /d %~dp0
start "" "Git-Svn-Console.exe" %path_%