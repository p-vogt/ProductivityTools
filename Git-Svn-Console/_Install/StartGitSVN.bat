@echo off
setlocal ENABLEDELAYEDEXPANSION
set path=%1
set path=%path:\=/%
cd /D "D:\GitPrivate\ProductivityTools\Git-Svn-Console\bin\Debug\"
start "" "D:\GitPrivate\ProductivityTools\Git-Svn-Console\bin\Debug\Git-Svn-Console.exe" %path%