cd src
"%ProgramFiles(x86)%\MSBuild\14.0\Bin\msbuild.exe" build.proj /target:Test /verbosity:normal
if errorlevel 1 pause
cd..
