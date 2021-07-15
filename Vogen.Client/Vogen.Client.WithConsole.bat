@echo off
start /wait cmd /c ^(Vogen.Client.exe ^> out.log 2^>^&1^)
type out.log
pause
