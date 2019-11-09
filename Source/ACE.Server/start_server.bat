@cd %~dp0
@if not exist "PcapPlayer.dll" (echo please run the copy of this file residing in the output folder: .\bin\x64\XXXXX\netcoreapp2.1\
pause)
dotnet PcapPlayer.dll
