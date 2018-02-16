@echo off
setlocal ENABLEDELAYEDEXPANSION
set path=%1
set path=%path:\=/%
cd /d %~dp0
start "" "Git-Svn-Console.exe" %path%