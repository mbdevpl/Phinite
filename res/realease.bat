call cd ../bin

call mkdir binzip

call xcopy *.dll binzip /y
call xcopy Phinite.exe binzip /y
call xcopy Phinite.exe.config binzip /y

echo Set objArgs = WScript.Arguments > _zipIt.vbs

REM file version
echo Set fso = CreateObject("Scripting.FileSystemObject") >> _zipIt.vbs
echo fileVersion = fso.GetFileVersion(objArgs(1) ^& "Phinite.exe") >> _zipIt.vbs
REM echo Wscript.Quit >> _zipIt.vbs

REM zip
echo InputFolder = objArgs(0) >> _zipIt.vbs
echo ZipFile = objArgs(1) ^& "Phinite " ^& fileVersion ^& ".zip" >> _zipIt.vbs
echo CreateObject("Scripting.FileSystemObject").CreateTextFile(ZipFile, True).Write "PK" ^& Chr(5) ^& Chr(6) ^& String(18, vbNullChar) >> _zipIt.vbs
echo Set objShell = CreateObject("Shell.Application") >> _zipIt.vbs
echo Set source = objShell.NameSpace(InputFolder).Items >> _zipIt.vbs
echo objShell.NameSpace(ZipFile).CopyHere(source) >> _zipIt.vbs
echo wScript.Sleep 2000 >> _zipIt.vbs

call CScript _zipIt.vbs %CD%\binzip %CD%\

del _zipIt.vbs
rmdir /s /q binzip

pause
