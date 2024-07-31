@echo off

SETLOCAL

set "cwd=%~dp0"
set workroot=C:\WX_PCG
set workdir=%workroot%\Content

echo 1. Creating workspace ...
if not exist "%workroot%" (
    echo Workspace root: %workroot%
    mkdir "%workroot%"
) else (
    echo Workspace root already exists: %workroot% 
    echo Skipping ...
)
echo.
echo Creating soft links
mklink /D %workdir% %cwd%  


echo Workspace directory: %workdir%
echo.
echo.


set package=%UserProfile%\Documents\houdini19.5\packages

echo 2. Creating houdini package file ...
if not exist "%package%" (
    echo Package file: %package%
    mkdir "%package%"
) else (
    echo Package file already exists: %package% 
    echo Skipping ...
    
)

echo.
echo.

echo 3. Copy WX_HoudiniPackage to houdini packages file ...
copy %~dp0\WX_HoudiniPackage.json %package% 

echo.
echo.

echo 4. Delete houdini.env
del /f /s /q %UserProfile%\Documents\houdini19.5\houdini.env
echo.


//PAUSE



