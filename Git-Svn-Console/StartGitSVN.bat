@echo off
setlocal ENABLEDELAYEDEXPANSION
set p="%1"
set p=%p:\=/%
cd /d %~dp0
start "" "Git-Svn-Console.exe" %p%