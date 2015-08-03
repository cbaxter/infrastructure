@ECHO OFF

:: Prompt for product version
:: --------------------------------------------------
set /p Configuration=Configuration: %=%
set /p MajorVersion=Major Version: %=%
set /p MinorVersion=Minor Version: %=%
set /p RevisionNumber=Revision Number: %=%

:: Run MSBuild
:: --------------------------------------------------
cd src
"%ProgramFiles(x86)%\MSBuild\14.0\Bin\msbuild.exe" build.proj /target:Package  /verbosity:normal /property:Configuration=%Configuration% /property:MajorVersion=%MajorVersion% /property:MinorVersion=%MinorVersion% /property:RevisionNumber=%RevisionNumber%
if errorlevel 1 pause
cd..
