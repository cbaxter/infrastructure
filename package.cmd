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
%SYSTEMROOT%\Microsoft.NET\Framework\v4.0.30319\msbuild build.proj /property:Configuration=%Configuration% /property:MajorVersion=%MajorVersion% /property:MinorVersion=%MinorVersion% /property:RevisionNumber=%RevisionNumber%
if errorlevel 1 pause
cd..
